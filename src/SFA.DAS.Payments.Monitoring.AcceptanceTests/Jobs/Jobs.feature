﻿Feature: Jobs
	To allow Payments System Administrators and other interested stake-holders to know the current status of the payments infrastructure 
	the Payments Job Monitoring records the completion status of jobs.

Background:
	Given the payments are for the current collection year
	And the current collection period is R01

Scenario: Provider Earnings Job Started
	Given the earnings event service has received a provider earnings job
	When the earnings event service notifies the job monitoring service to record the job
	Then the job monitoring service should record the job
	And the job monitoring service should also record the messages generated by earning events service
	
Scenario: Provider Earnings Job Finished
	Given a provider earnings job has already been recorded		
	When the final messages for the job are sucessfully processed
	Then the job monitoring service should update the status of the job to show that it has completed
	
Scenario: Provider Earnings Job Finished With Errors
	Given a provider earnings job has already been recorded		
	When the final messages for the job are sucessfully processed
	Then the job monitoring service should update the status of the job to show that it has completed