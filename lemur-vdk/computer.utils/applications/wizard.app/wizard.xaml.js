class wizard 
{
    constructor(id) {
        this.id = id;
        App.eventHandler('createBtn', 'create', Event.MouseDown);
    }
    create() {
        var appName = App.getProperty('nameBox', 'Text').replace('.app', '');

        var appDirName = appName;

    	appDirName += '.app';
            
        const xamlPath = 'home/myApps/' + appDirName + '/' + appName + '.xaml';
        const xamljsPath =  xamlPath + '.js';

		const xamljsCode = 
`class ${appName.replace('.app', '')} {
	constructor(id, ...args) {
		// id == this processes' process id
		// useful for graphics & system stuff.
		// ...args is a placeholder. all window constructors are varargs and can
		// be passed any arguments from App.start('appName', arg1, arg2, ...);
		// there is yet to be a cli tool to do this.
    	this.id = id;
    }
}`;

        const xamlCode = 
`<UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:local="clr-namespace:System.Windows.Controls;assembly=PresentationFramework"
	mc:Ignorable="d" 
	d:DesignHeight="450" d:DesignWidth="800" Background="#474354" BorderBrush="White" BorderThickness="5">
	<Grid>
		<!-- put your controls here :D -->
		<Label Content="Hello"/>
	</Grid>
</UserControl>
`;

        File.write(xamljsPath, xamljsCode)
        File.write(xamlPath, xamlCode)

        call(`install '${appDirName}'`);
    }
}