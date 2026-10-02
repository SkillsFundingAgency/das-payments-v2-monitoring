Feature: PV2-4298 - Exclude payments from DAS Earnings from period end and submission metrics

Scenario: Payments from DAS Earnings are excluded from the metrics
	Given we are generating period end or submission metrics
	And payments include Apprenticeship and GSO Short Course payments
	When the metrics are generated for DC/SLD
	Then the payments generated from SLD Earnings are included in the metrics
	And the payments generated from DAS Earnings are not included in the metrics

Scenario: Payments from SLD earnings are included in the metrics (Regression)
	Given we are generating period end or submission metrics
	And payments have been generated for Apprenticeships
	When the metrics are generated for DC/SLD
	Then the payments generated from SLD Earnings are included in the metrics
	And the payments generated from DAS Earnings are not included in the metrics
