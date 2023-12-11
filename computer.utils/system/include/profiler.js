class Profiler {
    constructor() {
        this.segmentTime;
        this.endTime;
        this.markers = [];
        this.averages = [];
        this.stopwatch = new Stopwatch();
        this.segmentsCount = [];
    }
    start() {
        this.stopwatch.Start();
        this.segmentTime = 0;
    }
    drawProfile(){
    	const results = this.sample_average();
		const profilerWidth = app.getProperty('ProfilerPanel', 'ActualWidth') / 2;
		const fpsWidth = app.getProperty('framerateLabel', 'ActualWidth');
		const actualWidth = profilerWidth - fpsWidth;
		
		let totalTime = 0;
		
		for (const label in results)
		    totalTime += results[label];
		
		const xFactor = actualWidth / totalTime;
		
		for (const label in results) {
		    const time = results[label];
		    app.setProperty(label, 'Content', `${time / 10_000} ms ${label}`);
		    app.setProperty(label, 'Width', time * xFactor);
		}
    }
    set_marker(id) {
        const time = this.stopwatch.ElapsedTicks;
        const segmentDuration = time - this.segmentTime;
        this.markers[id] = segmentDuration;

        if (!this.segmentsCount.includes(id))
            this.segmentsCount[id] = 1;

        this.segmentsCount[id] += 1;

        if (!this.averages.includes(id))
            this.averages[id] = segmentDuration;

        const count = this.markers[id] < 1 ? this.markers[id] : 1;

        this.averages[id] = (this.averages[id] * count + segmentDuration) / (count + 1);

        this.segmentTime = time;
    }
    sample_immediate = () => this.markers;
    sample_average = () => this.averages;
}

return { Profiler : Profiler };