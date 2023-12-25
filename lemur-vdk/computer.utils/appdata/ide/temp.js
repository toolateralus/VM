{
	let a = [];
	
	for (const i in range(0, 2500)) {
		a[i]=0;
	}
	
	let json = JSON.stringify(a);
	call ("install 'eval(`${a}`)'")
	
}



 this file can be found at 'computer/appdata/ide/temp.js'