using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.UNetWeaver
{
    class SyncListStructProcessor
    {
        TypeDefinition m_TypeDef;
        TypeReference m_ItemType;
        Weaver m_Weaver;
        const MethodAttributes kPublicStaticHide = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
        const MethodAttributes kPublicVirtualHide = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;

        public SyncListStructProcessor(TypeDefinition typeDef, Weaver weaver)
        {
            m_TypeDef = typeDef;
            m_Weaver = weaver;
            m_Weaver.DLog(typeDef, "SyncListStructProcessor for " + typeDef.Name);
        }

        public void Process()
        {
            // find item type
            var gt = (GenericInstanceType)m_TypeDef.BaseType;
            if (gt.GenericArguments.Count == 0)
            {
                m_Weaver.fail = true;
                Log.Error("SyncListStructProcessor no generic args");
                return;
            }
            m_ItemType = m_Weaver.m_ScriptDef.MainModule.ImportReference(gt.GenericArguments[0]);

            m_Weaver.DLog(m_TypeDef, "SyncListStructProcessor Start item:" + m_ItemType.FullName);

            m_Weaver.ResetRecursionCount();
            var writeItemFunc = GenerateSerialization();
            if (m_Weaver.fail)
            {
                return;
            }

            var readItemFunc = GenerateDeserialization();

            if (readItemFunc == null || writeItemFunc == null)
                return;

            GenerateReadFunc(readItemFunc);
            GenerateWriteFunc(writeItemFunc);

            m_Weaver.DLog(m_TypeDef, "SyncListStructProcessor Done");
        }

        /* deserialization of entire list. generates code like:
         *
            static public void ReadStructBuf(NetworkReader reader, SyncListBuf instance)
            {
                ushort count = reader.ReadUInt16();
                instance.Clear()
                for (ushort i = 0; i < count; i++)
                {
                    instance.AddInternal(instance.DeserializeItem(reader));
                }
            }
         */
        void GenerateReadFunc(MethodReference readItemFunc)
        {
            var functionName = "_ReadStruct" + m_TypeDef.Name + "_";
            if (m_TypeDef.DeclaringType != null)
            {
                functionName += m_TypeDef.DeclaringType.Name;
            }
            else
            {
                functionName += "None";
            }

            // create new reader for this type
            MethodDefinition readerFunc = new MethodDefinition(functionName, kPublicStaticHide, m_Weaver.voidType);

            readerFunc.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, m_Weaver.m_ScriptDef.MainModule.ImportReference(m_Weaver.NetworkReaderType)));
            readerFunc.Parameters.Add(new ParameterDefinition("instance", ParameterAttributes.None, m_TypeDef));

            readerFunc.Body.Variables.Add(new VariableDefinition(m_Weaver.uint16Type));
            readerFunc.Body.Variables.Add(new VariableDefinition(m_Weaver.uint16Type));
            readerFunc.Body.InitLocals = true;

            ILProcessor worker = readerFunc.Body.GetILProcessor();

            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Callvirt, m_Weaver.NetworkReadUInt16));
            worker.Append(worker.Create(OpCodes.Stloc_0));

            // Call Clear() from the base class
            worker.Append(worker.Create(OpCodes.Ldarg_1));
            MethodReference genericClearMethod = Helpers.MakeHostInstanceGeneric(m_Weaver.SyncListClear, m_ItemType);
            worker.Append(worker.Create(OpCodes.Callvirt, genericClearMethod));

            worker.Append(worker.Create(OpCodes.Ldc_I4_0));
            worker.Append(worker.Create(OpCodes.Stloc_1));
            var loopCheckLabel = worker.Create(OpCodes.Nop);
            worker.Append(worker.Create(OpCodes.Br, loopCheckLabel));

            // loop body
            var loopHeadLabel = worker.Create(OpCodes.Nop);
            worker.Append(loopHeadLabel);

            worker.Append(worker.Create(OpCodes.Ldarg_1));
            worker.Append(worker.Create(OpCodes.Ldarg_1));

            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Callvirt, readItemFunc));

            // call the generic AddInternal from the base class
            var addInternal = m_Weaver.ResolveMethod(m_Weaver.SyncListStructType, "AddInternal");
            var addInternalTyped = Helpers.MakeHostInstanceGeneric(addInternal, m_ItemType);
            worker.Append(worker.Create(OpCodes.Callvirt, addInternalTyped));

            worker.Append(worker.Create(OpCodes.Ldloc_1));
            worker.Append(worker.Create(OpCodes.Ldc_I4_1));
            worker.Append(worker.Create(OpCodes.Add));
            worker.Append(worker.Create(OpCodes.Conv_U2));
            worker.Append(worker.Create(OpCodes.Stloc_1));

            // loop check
            worker.Append(loopCheckLabel);
            worker.Append(worker.Create(OpCodes.Ldloc_1));
            worker.Append(worker.Create(OpCodes.Ldloc_0));
            worker.Append(worker.Create(OpCodes.Blt, loopHeadLabel));

            // done
            //worker.Append(worker.Create(OpCodes.Ldloc_1));
            worker.Append(worker.Create(OpCodes.Ret));

            m_Weaver.RegisterReadByReferenceFunc(m_TypeDef.FullName, readerFunc);
        }

        /*serialization of entire list. generates code like:
         *
            static public void WriteStructBuf(NetworkWriter writer, SyncListBuf items)
            {
                ushort count = (ushort)items.Count;
                writer.Write(count);
                for (ushort i=0; i < count; i++)
                {
                    items.SerializeItem(writer, items.GetItem(i));
                }
            }
         */
        void GenerateWriteFunc(MethodReference writeItemFunc)
        {
            var functionName = "_WriteStruct" + m_TypeDef.GetElementType().Name + "_";
            if (m_TypeDef.DeclaringType != null)
            {
                functionName += m_TypeDef.DeclaringType.Name;
            }
            else
            {
                functionName += "None";
            }

            // create new writer for this type
            MethodDefinition writerFunc = new MethodDefinition(functionName, kPublicStaticHide, m_Weaver.voidType);

            writerFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, m_Weaver.m_ScriptDef.MainModule.ImportReference(m_Weaver.NetworkWriterType)));
            writerFunc.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, m_Weaver.m_ScriptDef.MainModule.ImportReference(m_TypeDef)));

            writerFunc.Body.Variables.Add(new VariableDefinition(m_Weaver.uint16Type));
            writerFunc.Body.Variables.Add(new VariableDefinition(m_Weaver.uint16Type));
            writerFunc.Body.InitLocals = true;

            ILProcessor worker = writerFunc.Body.GetILProcessor();

            worker.Append(worker.Create(OpCodes.Ldarg_1));

            // call the generic Count from the base class
            var getCount = m_Weaver.ResolveMethod(m_Weaver.SyncListStructType, "get_Count");
            var getCountTyped = Helpers.MakeHostInstanceGeneric(getCount, m_ItemType);
            worker.Append(worker.Create(OpCodes.Callvirt, getCountTyped));

            worker.Append(worker.Create(OpCodes.Stloc_0));
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldloc_0));
            worker.Append(worker.Create(OpCodes.Callvirt, m_Weaver.NetworkWriteUInt16));
            worker.Append(worker.Create(OpCodes.Ldc_I4_0));
            worker.Append(worker.Create(OpCodes.Stloc_1));

            var loopCheckLabel = worker.Create(OpCodes.Nop);
            worker.Append(worker.Create(OpCodes.Br, loopCheckLabel));

            //loop start
            var loopStartLabel = worker.Create(OpCodes.Nop);
            worker.Append(loopStartLabel);

            worker.Append(worker.Create(OpCodes.Ldarg_1));
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldarg_1));
            worker.Append(worker.Create(OpCodes.Ldloc_1));

            // call the generic [] from the base class
            var getItem = m_Weaver.ResolveMethod(m_Weaver.SyncListStructType, "GetItem");
            var getItemTyped = Helpers.MakeHostInstanceGeneric(getItem, m_ItemType);
            worker.Append(worker.Create(OpCodes.Callvirt, getItemTyped));
            worker.Append(worker.Create(OpCodes.Callvirt, writeItemFunc));

            worker.Append(worker.Create(OpCodes.Ldloc_1));
            worker.Append(worker.Create(OpCodes.Ldc_I4_1));
            worker.Append(worker.Create(OpCodes.Add));
            worker.Append(worker.Create(OpCodes.Conv_U2));
            worker.Append(worker.Create(OpCodes.Stloc_1));

            worker.Append(loopCheckLabel);
            worker.Append(worker.Create(OpCodes.Ldloc_1));
            worker.Append(worker.Create(OpCodes.Ldloc_0));
            worker.Append(worker.Create(OpCodes.Blt, loopStartLabel));

            worker.Append(worker.Create(OpCodes.Ret));

            m_Weaver.RegisterWriteFunc(m_TypeDef.FullName, writerFunc);
        }

        // serialization of individual element
        MethodReference GenerateSerialization()
        {
            m_Weaver.DLog(m_TypeDef, "  GenerateSerialization");
            foreach (var m in m_TypeDef.Methods)
            {
                if (m.Name == "SerializeItem")
                    return m;
            }

            MethodDefinition serializeFunc = new MethodDefinition("SerializeItem", kPublicVirtualHide, m_Weaver.voidType);

            serializeFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, m_Weaver.m_ScriptDef.MainModule.ImportReference(m_Weaver.NetworkWriterType)));
            serializeFunc.Parameters.Add(new ParameterDefinition("item", ParameterAttributes.None, m_ItemType));
            ILProcessor serWorker = serializeFunc.Body.GetILProcessor();

            if (m_ItemType.IsGenericInstance)
            {
                m_Weaver.fail = true;
                Log.Error("GenerateSerialization for " + Helpers.PrettyPrintType(m_ItemType) + " failed. Struct passed into SyncListStruct<T> can't have generic parameters");
                return null;
            }

            foreach (var field in m_ItemType.Resolve().Fields)
            {
                if (field.IsStatic || field.IsPrivate || field.IsSpecialName)
                    continue;

                var importedField = m_Weaver.m_ScriptDef.MainModule.ImportReference(field);
                var ft = importedField.FieldType.Resolve();

                if (ft.HasGenericParameters)
                {
                    m_Weaver.fail = true;
                    Log.Error("GenerateSerialization for " + m_TypeDef.Name + " [" + ft + "/" + ft.FullName + "]. UNet [MessageBase] member cannot have generic parameters.");
                    return null;
                }

                if (ft.IsInterface)
                {
                    m_Weaver.fail = true;
                    Log.Error("GenerateSerialization for " + m_TypeDef.Name + " [" + ft + "/" + ft.FullName + "]. UNet [MessageBase] member cannot be an interface.");
                    return null;
                }

                MethodReference writeFunc = m_Weaver.GetWriteFunc(field.FieldType);
                if (writeFunc != null)
                {
                    serWorker.Append(serWorker.Create(OpCodes.Ldarg_1));
                    serWorker.Append(serWorker.Create(OpCodes.Ldarg_2));
                    serWorker.Append(serWorker.Create(OpCodes.Ldfld, importedField));
                    serWorker.Append(serWorker.Create(OpCodes.Call, writeFunc));
                }
                else
                {
                    m_Weaver.fail = true;
                    Log.Error("GenerateSerialization for " + m_TypeDef.Name + " unknown type [" + ft + "/" + ft.FullName + "]. UNet [MessageBase] member variables must be basic types.");
                    return null;
                }
            }
            serWorker.Append(serWorker.Create(OpCodes.Ret));

            m_TypeDef.Methods.Add(serializeFunc);
            return serializeFunc;
        }

        MethodReference GenerateDeserialization()
        {
            m_Weaver.DLog(m_TypeDef, "  GenerateDeserialization");
            foreach (var m in m_TypeDef.Methods)
            {
                if (m.Name == "DeserializeItem")
                    return m;
            }

            MethodDefinition serializeFunc = new MethodDefinition("DeserializeItem", kPublicVirtualHide, m_ItemType);

            serializeFunc.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, m_Weaver.m_ScriptDef.MainModule.ImportReference(m_Weaver.NetworkReaderType)));

            ILProcessor serWorker = serializeFunc.Body.GetILProcessor();

            serWorker.Body.InitLocals = true;
            serWorker.Body.Variables.Add(new VariableDefinition(m_ItemType));

            // init item instance
            serWorker.Append(serWorker.Create(OpCodes.Ldloca, 0));
            serWorker.Append(serWorker.Create(OpCodes.Initobj, m_ItemType));


            foreach (var field in m_ItemType.Resolve().Fields)
            {
                if (field.IsStatic || field.IsPrivate || field.IsSpecialName)
                    continue;

                var importedField = m_Weaver.m_ScriptDef.MainModule.ImportReference(field);
                var ft = importedField.FieldType.Resolve();

                MethodReference readerFunc = m_Weaver.GetReadFunc(field.FieldType);
                if (readerFunc != null)
                {
                    serWorker.Append(serWorker.Create(OpCodes.Ldloca, 0));
                    serWorker.Append(serWorker.Create(OpCodes.Ldarg_1));
                    serWorker.Append(serWorker.Create(OpCodes.Call, readerFunc));
                    serWorker.Append(serWorker.Create(OpCodes.Stfld, importedField));
                }
                else
                {
                    m_Weaver.fail = true;
                    Log.Error("GenerateDeserialization for " + m_TypeDef.Name + " unknown type [" + ft + "]. UNet [SyncVar] member variables must be basic types.");
                    return null;
                }
            }
            serWorker.Append(serWorker.Create(OpCodes.Ldloc_0));
            serWorker.Append(serWorker.Create(OpCodes.Ret));

            m_TypeDef.Methods.Add(serializeFunc);
            return serializeFunc;
        }
    }
}
