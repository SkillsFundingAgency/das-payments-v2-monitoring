# Payments V2 Monitoring

<img src="https://avatars.githubusercontent.com/u/9841374?s=200&v=4" align="right" alt="UK Government logo">

[![Build Status](https://dev.azure.com/sfa-gov-uk/DCT/_apis/build/status/GitHub/Service%20Fabric/SkillsFundingAgency.das-payments-v2-monitoring?branchName=main)](https://dev.azure.com/sfa-gov-uk/DCT/_apis/build/status/GitHub/Service%20Fabric/SkillsFundingAgency.das-payments-v2-monitoring?branchName=main)
[![Jira Project](https://img.shields.io/badge/Jira-Project-blue)](https://skillsfundingagency.atlassian.net/secure/RapidBoard.jspa?rapidView=782&projectKey=PV2)
[![Confluence Project](https://img.shields.io/badge/Confluence-Project-blue)](https://skillsfundingagency.atlassian.net/wiki/spaces/NDL/pages/3700621400/Provider+and+Employer+Payments+Payments+BAU)
[![License](https://img.shields.io/badge/license-MIT-lightgrey.svg?longCache=true&style=flat-square)](https://en.wikipedia.org/wiki/MIT_License)


The Payments V2 Monitoring ServiceFabric application provides functionality for monitoring the statuses of jobs as they are processed by the system and recording associated metrics.

## How It Works

This repository contains stateful and stateless ServiceFabric services that monitor the status of jobs that have been initiated by other applications within the Payments V2 system. For example, the services will record information when a job starts, stops, errors, or has timed out.

This repository also contains an Azure Function application that is responsible for generating and recording metrics about the period end run, for example, successful submissions for the collection period.

## 🚀 Installation

### Pre-Requisites

Setup instructions can be found at the following link, which will help you set up your environment and access the correct repositories: https://skillsfundingagency.atlassian.net/wiki/spaces/NDL/pages/950927878/Development+Environment+-+Payments+V2+DAS+Space

### Config

As detailed in: https://skillsfundingagency.atlassian.net/wiki/spaces/NDL/pages/644972941/Developer+Configuration+Settings

Select the configuration for the Monitoring application

## 🔗 External Dependencies

N/A

## Technologies

* .NetCore 2.1/3.1/6
* Azure SQL Server
* Azure Functions
* Azure Service Bus
* ServiceFabric

## 🐛 Known Issues

N/A

