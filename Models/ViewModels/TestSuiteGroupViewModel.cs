public class TestSuiteGroupViewModel
{
    public string TestSuiteName { get; set; }
    public string TestSuitePath { get; set; }
    public TestSuite TestSuite { get; set; }
    public ICollection<TestCase> TestCases { get; set; }
    public List<ScheduledTestViewModel> ScheduledRuns { get; set; }
}