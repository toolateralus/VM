class xaml_js_project_generator{
	saveFiles(root, files, paths) {

		for(var i = 0; i < files.length; ++i) {
			
			const file = files[i];
			const path = root + '/' + paths[i];
			
			if (file !== null && file !== '' && path !== null && path !== ''){
				file.write(path, file)
			}
		}

	}

	createProjDir(dir, name, ext)
	{
		var path = "";
		if (dir !== '') {
			path = dir + '/' + name + '.' + ext;
		}
		else { 
			path = 'apps' + '/' + name + '.' + ext;
		}

		call(`mkdir ${path}`);
		return path;
	}
}

{

	print('enter the app name');
	let name = interop.read();
	print(name);
	print('enter the desired extension ( .app | .web )');
	let ext = interop.read();
	print(ext);


	print('{optional} enter a target directory for the project, or just press enter');
	let dir = interop.read();
	print(dir);

	const generator = new xaml_js_project_generator();
	print(generator);

	if (ext === '.app')
	{
		let template_js = "";
		template_js = file.read('template_app.xaml.js')
		
		let template_xaml = "";
		template_xaml = file.read('template_app.xaml')
		
		template_js = template_js.replace('template', name);
		template_xaml = template_xaml.replace('template', name);
		
		const path = generator.createProjDir(dir, name, ext);
		generator.saveFiles(path, [template_js, template_xaml], [`${name}.xaml.js`,`${name}.xaml`])
	}
}