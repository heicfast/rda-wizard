using System.Windows.Markup;

// XML-namespace federation: pages reference converters/views/services with
// dotted names under the single app xmlns (xmlns:app="clr-namespace:RdaWizard.App"),
// e.g. <app:Converters.ByteSizeConverter> and <app:Views.HomeView>.

[assembly: XmlnsDefinition("clr-namespace:RdaWizard.App", "RdaWizard.App.Converters")]
[assembly: XmlnsDefinition("clr-namespace:RdaWizard.App", "RdaWizard.App.Views")]
[assembly: XmlnsDefinition("clr-namespace:RdaWizard.App", "RdaWizard.App.Services")]
[assembly: XmlnsPrefix("clr-namespace:RdaWizard.App", "app")]
