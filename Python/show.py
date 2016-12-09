import numpy as np
import matplotlib.pyplot as plt
import matplotlib.cm as cm
from matplotlib import colors
from scipy import misc
import argparse

frame = 0

parser = argparse.ArgumentParser(description='Display recorded Kinect data')
parser.add_argument('-f', dest='frame', action='store', type=int, default=0, help='Which frame do you want to display')

args = parser.parse_args()
frame = args.frame

#COLOR
color = np.fromfile('COLOR/%d.uint8' % frame, dtype=np.uint8)
color.shape = (1080, 1920, 4)
plt.subplot(2, 2, 1)
plt.imshow(color)
plt.title('Original color 1920x1080')

#DEPTH
min_depth = 500  # Min reliable depth
max_depth = 4500 # Max reliable depth

max_value = np.iinfo(np.uint16).max

depth = np.fromfile('DEPTH/%d.uint16' % frame, dtype=np.uint16)
depth.shape = (424, 512)

plt.subplot(2, 2, 2)
plt.imshow(depth, interpolation='nearest', cmap=cm.gist_heat)
plt.title('Depth values 512x424')

#INDEX
index = np.fromfile('INDEX/%d.uint8' % frame, dtype=np.uint8)
index.shape = (424, 512)
index[index == 255] = 8

index_color_map = colors.ListedColormap(['red', 'green', 'blue', 'cyan', 'magenta', 'yellow', '#ff7700', 'white', 'black'])

plt.subplot(2, 2, 3)
plt.imshow(index, interpolation='nearest', cmap=index_color_map)
plt.title('Segmentation data 512x424')

#TRACKEDCOLOR
tracked = np.fromfile('TRACKEDCOLOR/%d.uint8' % frame, dtype=np.uint8);
tracked.shape = (424, 512, 4)
plt.subplot(2, 2, 4)
plt.imshow(tracked)
plt.title('Color mapped to tracked depth space 512x424')


plt.show()
