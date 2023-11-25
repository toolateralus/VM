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