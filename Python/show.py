import numpy as np
import matplotlib.pyplot as plt
import matplotlib.cm as cm
from matplotlib import colors
from scipy import misc
import argparse
import os

parser = argparse.ArgumentParser(description='Display recorded Kinect data', fromfile_prefix_chars="@")
parser.add_argument('-p', dest='recording_path', action='store', type=str, default="", help="Which recording do you want to display?")
parser.add_argument('-f', dest='frame', action='store', type=int, default=0, help='Which frame do you want to display')

args = parser.parse_args()
frame = args.frame

recording_path = args.recording_path.strip(os.sep)

#COLOR
colorFile = os.path.join(recording_path, 'COLOR', '%d.uint8' % frame)
if (os.path.isfile(colorFile)):
    color = np.fromfile(colorFile, dtype=np.uint8)
    color.shape = (1080, 1920, 4)
    plt.subplot(2, 2, 1)
    plt.imshow(color)
    plt.title('Original color 1920x1080')

#DEPTH
depthFile = os.path.join(recording_path, 'DEPTH', '%d.uint16' % frame)
if (os.path.isfile(depthFile)):
    min_depth = 500  # Min reliable depth
    max_depth = 4500 # Max reliable depth

    max_value = np.iinfo(np.uint16).max

    depth = np.fromfile(depthFile, dtype=np.uint16)
    depth.shape = (424, 512)

    plt.subplot(2, 2, 2)
    plt.imshow(depth, interpolation='nearest', cmap=cm.gist_heat)
    plt.title('Depth values 512x424')

#INDEX
indexFile = os.path.join(recording_path, 'INDEX', '%d.uint8' % frame)
if (os.path.isfile(indexFile)):
    index = np.fromfile(indexFile, dtype=np.uint8)
    index.shape = (424, 512)
    index[index == 255] = 8

    index_color_map = colors.ListedColormap(['red', 'green', 'blue', 'cyan', 'magenta', 'yellow', '#ff7700', 'white', 'black'])

    plt.subplot(2, 2, 3)
    plt.imshow(index, interpolation='nearest', cmap=index_color_map)
    plt.title('Segmentation data 512x424')

#TRACKEDCOLOR
trackedFile = os.path.join(recording_path, 'TRACKEDCOLOR', '%d.uint8' % frame)
if (os.path.isfile(trackedFile)):
    tracked = np.fromfile(trackedFile, dtype=np.uint8);
    tracked.shape = (424, 512, 4)
    plt.subplot(2, 2, 4)
    plt.imshow(tracked)
    plt.title('Color mapped to tracked depth space 512x424')

plt.show()
