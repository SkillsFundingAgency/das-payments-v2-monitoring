Feature: PV2-4298 - Exclude payments from DAS Earnings from period end

Scenario: Payments from DAS Earnings are excluded from the metrics
	Given we are generating period end metrics
	And payments have been generated for Apprenticeship and GSO Short Course payments
	When the metrics are generated for DC/SLD
	Then only SLD earnings are included in the funding-source metrics
	And only SLD learning payments are included in transaction-type metrics
	And only SLD learners are included in the in-learning metrics
	And only SLD payments are included in year-to-date metrics
    And only SLD learner payments are returned

Scenario: Payments from SLD earnings are included in the metrics (Regression)
	Given we are generating period end metrics
	And payments have been generated for Apprenticeships
	When the metrics are generated for DC/SLD
	Then only SLD earnings are included in the funding-source metrics
	And only SLD learning payments are included in transaction-type metrics
	And only SLD learners are included in the in-learning metrics
	And only SLD payments are included in year-to-date metrics
    And only SLD learner payments are returned

Scenario: Legacy SLD payments with no funding platform are included in the metrics (Regression/ legacy)
	Given we are generating period end metrics
	And legacy SLD payments and DAS payments have been generated
	When the metrics are generated for DC/SLD
	Then only SLD earnings are included in the funding-source metrics
    And only SLD learning payments are included in transaction-type metrics
	And only SLD learners are included in the in-learning metrics
	And only SLD payments are included in year-to-date metrics
	And only SLD learner payments are returned

Scenario: DAS payments are excluded from already-paid data-lock earnings
    Given we are generating period end metrics
    And historical payments from SLD and DAS are associated with data-lock events
	When GetAlreadyPaidDataLockProviderTotals is called
	Then only historical SLD payments are included in already-paid data-lock earnings
