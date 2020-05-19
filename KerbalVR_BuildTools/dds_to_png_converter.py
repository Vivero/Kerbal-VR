from wand import image
import argparse, os, re

argparser = argparse.ArgumentParser(description='Converts dds files to png in batch')
argparser.add_argument('dir', nargs=1, help='directory to recursively search for dds files')
args = argparser.parse_args()

input_dir = args.dir[0]

# convert all dds files in this directory
def convert_all_dds_to_png(dir_path):
    print('processing directory: {0}'.format(dir_path))

    # get dds files in this directory
    dds_files = [f for f in os.listdir(dir_path) if os.path.isfile(os.path.join(dir_path, f)) and f.endswith('.dds')]

    # convert these files, deleting the originals
    for dds_filename in dds_files:
        dds_filepath = os.path.join(dir_path, dds_filename)
        print('dds_filepath={0}'.format(dds_filepath))
        png_filepath = re.sub(r'\.dds$', '.png', dds_filepath)
        with image.Image(filename=dds_filepath) as img:
            img.compression = 'no'
            img.flip()
            img.save(filename=png_filepath)
        os.remove(dds_filepath)

    # get sub-directories
    subdirs = [f for f in os.listdir(dir_path) if os.path.isdir(os.path.join(dir_path, f))]
    for subdir in subdirs:
        subdir_path = os.path.join(dir_path, subdir)
        convert_all_dds_to_png(subdir_path)

# process this directory
convert_all_dds_to_png(input_dir)

