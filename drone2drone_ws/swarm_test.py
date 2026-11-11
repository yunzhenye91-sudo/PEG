#!/usr/bin/env python2

from math import cos, cosh, pi, sin
import time
import numpy as np

from numpy.core.records import array
import rospy

import time
from threading import Thread

# import queue

from swarm_controller import *
# import xlsxwriter 


def main():

	rospy.init_node('tello_swarm_test_node', anonymous=True)

	num_of_drones = 3
	# initial_positions = [
	# 	[0,0],
	# 	[1,0]
	# ]

	# objective_positions = [
	# 	[4,4,2],
	# 	[3,4,2],
	# 	[5.5,4,2]
	# ]

	# objective_positions = [
	# 	[4.0,	4.0,	2],
	# 	[4.8,	4.0,	2],
	# 	[5.6,	4.0, 	2],
	# 	[4.8,	3.0,	2],
	# 	[4.9,	2.2, 	2],
	# 	[4.9, 	1.6, 	2]
	# ]

	# uwb_tags = [
	# 	"2D61",
	# 	"2B13",
	# 	"0CC8",
	# 	"2891",
	# 	"0D3B",
	# 	"28C0",
	# 	"2B26",
	# 	"8A09"
	# ]

	objective_positions = [
		[2.5,	0,	1],
		[0.5, 0.0, 1.5],
		[0, 1, 1]
	]

	objective_paths = [
		[[0,	0,	1], [0, 0, 1]],
		[[0.5, 0.0, 1.5], [1.5, 0.0, 1.5]],
		[[0, 1, 1], [1, 1, 1]]
	]
	
	uwb_tags = [
		"tello0",
		"tello1",
		"tello2"
	]

	swarm = UWBTelloSwarm(num_of_drones, objective_positions, uwb_tags, objective_paths)

	# design a trajectory
	pos_traj = []
	ini_obj_position= swarm.get_agent_pos(0).copy()

	# pos_traj = generate_s_trajectory(ini_obj_position)

	swarm.parallel_takeoff()
	#swarm.agent_takeoff(0)
	# swarm.parallel_takeoff_positioning()
	time.sleep(5)
	#swarm.parallel_takeoff()
	# set the trajectory
	
	# swarm.set_trajectory(0, pos_traj)
	
	# swarm.run() # run the drone to move
	swarm.run_rlInference()

	# swarm.run_agent()
	swarm.parallel_land()
	rospy.loginfo("Landing swarm.")

def generate_hovering(ini_obj_position):
	pos_traj = []
	cur_obj_position = ini_obj_position.copy()
	cur_obj_position[2] += 1.0
	#Takeoff and hover to wait for car
	for i in range(0, 1500000): 
		pos_traj.append(cur_obj_position.copy())

	return pos_traj

def generate_circle(ini_obj_position):
	pos_traj = []
	cur_obj_position = ini_obj_position.copy()
	cur_obj_position[2] += 1.5
	#Takeoff and hover to wait for car
	for i in range(0, 500): 
		pos_traj.append(cur_obj_position.copy())

	for i in range(0, 1500000): 
		cur_obj_position[0] = ini_obj_position[0] + (1 * sin(0.002*i))  #Omega = 0.001
		cur_obj_position[1] = ini_obj_position[1] + (1 * cos(0.002*i)) -1
		pos_traj.append(cur_obj_position.copy())
	return pos_traj

def generate_s_trajectory(ini_obj_position):
	pos_traj = []
	cur_obj_position = ini_obj_position.copy()
	cur_obj_position[2] += 1.5
	
	#Takeoff and hover to wait for car
	for i in range(0, 500): 
		pos_traj.append(cur_obj_position.copy())

	sign = -1
	for i in range(0, 1000):
		cur_obj_position[1] = ini_obj_position[1] + (1 * sin(0.002*pi*i))
		if i % 1000 == 0:
			sign = -1*sign
		cur_obj_position[0] += sign*(0.001)
		pos_traj.append(cur_obj_position.copy())
		
	for i in range(0, 500): 
		pos_traj.append(cur_obj_position.copy())
	return pos_traj



if __name__ == '__main__':
	main()
