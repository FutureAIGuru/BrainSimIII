#
# PROPRIETARY AND CONFIDENTIAL
# Brain Simulator 3 v.1.0
# © 2022 FutureAI, Inc., all rights reserved
#

*** Settings ***
Documentation		This testset runs with no setup or teardown whatsoever, 
...					basic tests for starting and stopping programs.

Library   			testtoolkit.py
Library   			teststeps.py

Resource			keywords.resource

*** Test Cases ***

Are Prerequisites Taken Care Of?
	[Tags]              Complete
	${Result}    		Check Test Requirements
	
Can We Clear Appdata?
	[Tags]              Complete
	${Result}    		Clear Appdata
	Should Be True		${Result}

# This immediately follows the above test so we can reset the checkmark.
Can We Start Brain Simulator III with Getting Started?
	[Tags]              Complete
	Start Brain Simulator with Getting Started

Can We Stop Brain Simulator III?
	[Tags]              Complete
	Stop Brain Simulator

Can We Start Neuron Server?
	[Tags]              Complete
	${Result}    		Start Neuron Server

Can We Stop Neuron Server?
	[Tags]              Complete
	${Result}    		Stop Neuron Server
