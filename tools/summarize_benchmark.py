"""Summarize integrated-slice benchmark CSVs (T11).

Per run: frames, median/p95/p99/max frame time, frames over 33.3 ms, CPU/GPU
medians (they overlap and are never added), peak allocated memory and peak live
projectiles. Targets are the D02 prototype benchmark: median <= 16.7 ms and
p95 <= 20 ms. Writes summary.json and summary.md next to the CSVs.
"""
import csv
import glob
import json
import os
import sys


def percentile(values, q):
    ordered = sorted(values)
    if not ordered:
        return float('nan')
    k = (len(ordered) - 1) * q
    lo = int(k)
    hi = min(lo + 1, len(ordered) - 1)
    return ordered[lo] + (ordered[hi] - ordered[lo]) * (k - lo)


def summarize(path):
    frames, cpu, gpu, allocated, shots, regions = [], [], [], [], [], set()
    with open(path, newline='') as handle:
        for row in csv.DictReader(handle):
            frames.append(float(row['frame_ms']))
            if float(row['cpu_ms']) > 0:
                cpu.append(float(row['cpu_ms']))
            if float(row['gpu_ms']) > 0:
                gpu.append(float(row['gpu_ms']))
            allocated.append(int(float(row['allocated_bytes'])))
            shots.append(int(row['live_shots']))
            regions.add(row['region'])
    name = os.path.basename(path)[:-4]
    median, p95 = percentile(frames, 0.5), percentile(frames, 0.95)
    return {
        'run': name,
        'frames': len(frames),
        'seconds': round(sum(frames) / 1000.0, 1),
        'median_ms': round(median, 3),
        'p95_ms': round(p95, 3),
        'p99_ms': round(percentile(frames, 0.99), 3),
        'max_ms': round(max(frames) if frames else float('nan'), 3),
        'over_33ms': sum(1 for f in frames if f > 33.3),
        'cpu_median_ms': round(percentile(cpu, 0.5), 3) if cpu else None,
        'gpu_median_ms': round(percentile(gpu, 0.5), 3) if gpu else None,
        'peak_allocated_mb': round(max(allocated) / 1048576.0, 1) if allocated else None,
        'peak_live_shots': max(shots) if shots else 0,
        'regions': sorted(r for r in regions if r),
        'meets_median_target': median <= 16.7,
        'meets_p95_target': p95 <= 20.0,
    }


def main(folder):
    runs = [summarize(p) for p in sorted(glob.glob(os.path.join(folder, '*-run*.csv')))]
    with open(os.path.join(folder, 'summary.json'), 'w') as handle:
        json.dump(runs, handle, indent=2)
    lines = ['| Run | Frames | Median ms | p95 ms | p99 ms | Max ms | >33.3 ms | CPU med | GPU med | Peak MB | Peak shots | Regions |',
             '| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |']
    for r in runs:
        lines.append('| {run} | {frames} | {median_ms} | {p95_ms} | {p99_ms} | {max_ms} | {over_33ms} | {cpu} | {gpu} | {peak_allocated_mb} | {peak_live_shots} | {regions} |'.format(
            cpu=r['cpu_median_ms'] if r['cpu_median_ms'] is not None else 'n/a',
            gpu=r['gpu_median_ms'] if r['gpu_median_ms'] is not None else 'n/a',
            regions=', '.join(r['regions']), **{k: v for k, v in r.items() if k != 'regions'}))
    with open(os.path.join(folder, 'summary.md'), 'w') as handle:
        handle.write('\n'.join(lines) + '\n')
    print('\n'.join(lines))


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else '.')
