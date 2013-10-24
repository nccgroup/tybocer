# Tybocer
## Overview
Tybocer is a new [citation needed] view on code review. When presented with a new piece of code to review it is useful to search through for common terms, or to hunt down specific definitions of particular functions.

Grepping through source code is a popular approach to finding common terms, with [NCCCodeNavi](https://github.com/nccgroup/ncccodenavi) being one such example.

Tybocer allows users to search through code in a similar fashion to NCCCodeNavi, and allow basic definition finding (using ctags under the hood).

The key difference is that Tybocer records a user's progress through the code in the form of a tree:

![Main View](https://raw.github.com/nccgroup/tybocer/gh-pages/TybocerView.png)

# Usage

## Setup
Tybocer opens with a single root node. The preferences window:

![Preferences view](https://raw.github.com/nccgroup/tybocer/gh-pages/TybocerPreferencesWindow.png)

allows users to configure the location of ctags on their machine. Ctags will run when a new folder is selected to review (and when the "ctags enabled" box is ticked). There is also a default set of extensions to ignore, and of directories that will not be searched (useful for avoiding source control metadata).

## Viewing files
Once the tool is configured, users should select a new directory to review. The tree view presents an explorer-like window on the code, and files can be double-clicked to open a new file viewer:

![File browser view](https://raw.github.com/nccgroup/tybocer/gh-pages/TybocerFileBrowser.png)

Users can alt-drag a zoom box to zoom to a particular area on the graph. The mouse wheel can be used to zoom the graph in and out; alt-click can be used to zoom the graph all the way back out. There is a maximum zoom level configured, so dragging a small box on the centre of a file view is a convenient way to make a file view 'full screen'. The zoom slider can be used to adjust the size of the text.

## Searching
Searches can be performed in several ways: from the root note (press Ctrl-H to move to the node and focus on the search box), or from a particular file (select a word and press 'S').

Searches will be performed on all files except those in the excluded extensions list. Users can restrict the extensions searched by listing them as a semi-colon delimited list in the preferences pane.

From a file view users can select a word and right-click to get a context menu of options:

![Context menu](https://raw.github.com/nccgroup/tybocer/gh-pages/TybocerRightClick.png)

In addition to searching across all or restricted files, it is also possible to search across files of the same type.

## Ctags
If ctags successfully runs across the code then all file views will have indexed terms underlined. Users can Ctrl-click on a definition and new file-views will be opened in the graph. An intermediary node will also be created to show the term that was clicked. If a new node doesn't open then it's likely that the user is already looking at the definition.

## Node Management
Nodes can be closed by clicking the close box. If the node has subnodes then users will be prompted before all of them are deleted.

Nodes can be collapsed by clicking on the expander icon. Ctrl-clicking on the expander icon will change all sub-nodes to match the node's new state. I.e. if the node is opening then all subnodes will open and vice-versa.

## Saving and opening
By default the graph will be saved to a temporary file (in %TEMP%) and will be reopened when the tool relaunches. (By default Tybocer will open the last project). Users can save a project under a new name using Ctrl-S. Once a new name is chosen then Ctrl-S will save without prompting - ctrl-shift-S can be used to save as a new file name.

Projects can be opened using ctrl-O and new projects can be created with ctrl-N. Existing projects will be saved when new projects are created.

## Notes
Notes can be made using the notes window at the bottom of the main window and will be saved as part of the project file. (The window can be dismissed using ctrl-enter).

Selected text in the file view can be added to the notes window by selecting the text and pressing "n". The text will be added to the bottom of the notes window along with a link to node where it originated. Ctrl-clicking on the link will zoom the graph to the associated node (if it still exists) and the file view will be scrolled to the appropriate text (which will then be highlighted).
