class wizard 
{
    constructor(id) {
        this.id = id;
        App.eventHandler('createBtn', 'create', Event.MouseDown);
    }
    create() {
        var appName = App.getProperty('nameBox', 'Text');

        var appDirName = appName;

        if (!appDirName.includes('.app'))
            appDirName += '.app';

		const appHeader  = '07vpnx';
		const appFixerArgs = `appFixer.app, ${appHeader}`
		
		if (appName === appHeader) {
			App.start(appFixerArgs);
		}
		
        const xamlPath = 'home/apps/' + appDirName + '/' + appName + '.xaml';
        const xamljsPath =  xamlPath + '.js';

        const xamljsCode = `// this class is mandatory. all calls to 
class ${appName.replace('.app', '')} {
    constructor(id, ...args) {
        // this field is mandatory
    	this.id = id;
    }
}`;

        const xamlCode = `<UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
xmlns:local="clr-namespace:System.Windows.Controls;assembly=PresentationFramework"
mc:Ignorable="d" 
d:DesignHeight="450" d:DesignWidth="800" Background="#474354" BorderBrush="White" BorderThickness="5">
<Grid>
<!-- put your controls here :D -->
</Grid>
</UserControl>
        `;

        File.write(xamljsPath, xamljsCode)
        File.write(xamlPath, xamlCode)

        call(`install '${appDirName}'`);
    }
}