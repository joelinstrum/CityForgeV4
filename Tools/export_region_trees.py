"""Compatibility entry point for the Himalayan birch export.
Run Blender -b 'Betula utilis.blend' --python Tools/export_region_trees.py.
"""
import os, runpy, sys
sys.argv += ['--', 'birch']
runpy.run_path(os.path.join(os.path.dirname(__file__), 'export_tree_library.py'), run_name='__main__')
