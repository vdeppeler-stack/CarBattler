# Headless entry: blender -b -P process_vehicle_cli.py -- <id> <glb> <out_dir>
import sys, os
here = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(here, "process_vehicle.py")).read())
args = sys.argv[sys.argv.index("--") + 1:]
process(args[0], args[1], args[2])
