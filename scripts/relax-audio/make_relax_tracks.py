"""
Tạo 3 bài nhạc thư giãn cho Mom Ơi (GET /api/wellness/relax-tracks), tự tổng hợp
hoàn toàn bằng code nên không dính bản quyền. Thông số (nốt gốc, bpm, vòng hợp âm,
độ ồn nền) lấy đúng từ WellnessService.GetRelaxTracks.

Mỗi bài dài đúng một số nguyên ô nhịp (~3 phút); đuôi tiếng vang được gập về đầu bài
để phát lặp liền mạch.

Chạy: python make_relax_tracks.py <thư_mục_xuất>
"""
import sys
from pathlib import Path

import lameenc
import numpy as np

SR = 44100


# ── Khối âm thanh cơ bản ────────────────────────────────────────────────────────

def midi_to_hz(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def hz_to_midi(hz):
    return int(round(69 + 12 * np.log2(hz / 440.0)))


def piano_note(freq, dur, vel=0.5, brightness=1.0):
    """Piano mềm: cộng hoạ âm hơi lệch (inharmonic), hoạ âm cao tắt nhanh hơn."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for k in range(1, 9):
        f = freq * k * np.sqrt(1 + 0.0004 * k * k)
        if f > SR / 2.2:
            break
        amp = (1.0 / k ** (1.6 - 0.4 * brightness))
        decay = 0.9 + 2.2 * k / brightness
        out += amp * np.sin(2 * np.pi * f * t) * np.exp(-decay * t / max(dur, 0.6))
    attack = np.minimum(1.0, t / 0.008)
    release = np.minimum(1.0, (dur - t) / 0.25).clip(0, 1)
    return vel * out * attack * release / 2.2


def keys_note(freq, dur, vel=0.4):
    """Electric piano kiểu Rhodes cho lofi: hai sóng sin điều tần nhẹ, tremolo chậm."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    mod = 0.9 * np.exp(-3.0 * t) * np.sin(2 * np.pi * freq * 1.0 * t)
    tone = np.sin(2 * np.pi * freq * t + mod) + 0.25 * np.sin(2 * np.pi * freq * 2 * t)
    env = np.minimum(1.0, t / 0.01) * np.exp(-1.4 * t / max(dur, 0.8))
    release = np.minimum(1.0, (dur - t) / 0.3).clip(0, 1)
    trem = 1 - 0.12 * (1 + np.sin(2 * np.pi * 4.2 * t)) / 2
    return vel * tone * env * release * trem / 1.3


def bass_note(freq, dur, vel=0.5):
    n = int(dur * SR)
    t = np.arange(n) / SR
    tone = np.sin(2 * np.pi * freq * t) + 0.18 * np.sin(2 * np.pi * 2 * freq * t)
    env = np.minimum(1.0, t / 0.02) * np.exp(-0.9 * t / dur)
    release = np.minimum(1.0, (dur - t) / 0.08).clip(0, 1)
    return vel * tone * env * release


def kick(vel=0.5):
    dur = 0.45
    t = np.arange(int(dur * SR)) / SR
    f = 46 + 40 * np.exp(-t * 28)
    phase = 2 * np.pi * np.cumsum(f) / SR
    return vel * np.sin(phase) * np.exp(-t * 9)


def spectral_filter(x, low=None, high=None, tilt=0.0):
    """Lọc qua FFT trên cả tín hiệu: low/high là tần số cắt (Hz), tilt âm = tối hơn."""
    spec = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1 / SR)
    gain = np.ones_like(freqs)
    if high:
        gain *= 1 / np.sqrt(1 + (freqs / high) ** 4)
    if low:
        gain *= 1 / np.sqrt(1 + (low / np.maximum(freqs, 1e-3)) ** 4)
    if tilt:
        gain *= (np.maximum(freqs, 20) / 1000.0) ** tilt
    return np.fft.irfft(spec * gain, n=len(x))


def hat(rng, vel=0.08):
    dur = 0.06
    noise = rng.standard_normal(int(dur * SR))
    noise = spectral_filter(noise, low=7000)
    t = np.arange(len(noise)) / SR
    return vel * noise * np.exp(-t * 70)


def reverb(x, seconds=2.6, wet=0.32, rng=None):
    """Hồi âm bằng tích chập với đáp ứng xung giả lập (nhiễu tắt dần, tối dần)."""
    rng = rng or np.random.default_rng(7)
    n = int(seconds * SR)
    t = np.arange(n) / SR
    ir = rng.standard_normal(n) * np.exp(-t * 6.9 / seconds)
    ir = spectral_filter(ir, high=5000, tilt=-0.3)
    ir /= np.sqrt(np.sum(ir ** 2))
    size = len(x) + n
    fft_n = 1 << (size - 1).bit_length()
    wet_sig = np.fft.irfft(np.fft.rfft(x, fft_n) * np.fft.rfft(ir, fft_n), fft_n)[:size]
    dry = np.concatenate([x, np.zeros(n)])
    return (1 - wet) * dry + wet * wet_sig


def place(buf, sig, at):
    start = int(at * SR)
    end = min(len(buf), start + len(sig))
    if start < len(buf):
        buf[start:end] += sig[: end - start]


def chord_notes(root_midi, quality):
    shapes = {
        'maj': [0, 4, 7],
        'min': [0, 3, 7],
        'maj7': [0, 4, 7, 11],
        'min7': [0, 3, 7, 10],
    }
    return [root_midi + s for s in shapes[quality]]


def fold_loop(mix, loop_len):
    """Gập phần đuôi (vang, nốt còn ngân) về đầu bài để phát lặp liền mạch."""
    head = mix[:loop_len].copy()
    tail = mix[loop_len:]
    head[: len(tail)] += tail
    return head


def loop_noise(rng, n, **filter_args):
    """Nhiễu dài đúng một vòng lặp. Lọc FFT là lọc vòng, nên đầu và cuối tự khớp nhau."""
    x = spectral_filter(rng.standard_normal(n), **filter_args)
    return x / (np.max(np.abs(x)) + 1e-9)


def loop_impulses(rng, n, per_second, **filter_args):
    x = np.zeros(n)
    idx = rng.integers(0, n, size=int(n / SR * per_second))
    x[idx] = rng.standard_normal(len(idx))
    x = spectral_filter(x, **filter_args)
    return x / (np.max(np.abs(x)) + 1e-9)


def master(x):
    """Nén mềm và chuẩn hoá về mức nghe dịu, đỉnh không vượt -1 dBFS."""
    x = x - np.mean(x)
    x = np.tanh(x / (np.max(np.abs(x)) + 1e-9) * 1.4) / np.tanh(1.4)
    rms = np.sqrt(np.mean(x ** 2))
    return x * min(0.89, 0.16 / rms)


# ── Ba bài ──────────────────────────────────────────────────────────────────────

def piano_lullaby():
    """Piano dịu: C4, 52 bpm, I-V-vi-IV, rải hợp âm thưa, gần như không có nền."""
    rng = np.random.default_rng(52)
    bpm, bars = 52, 40
    beat = 60 / bpm
    bar = 4 * beat
    loop_len = int(bars * bar * SR)
    buf = np.zeros(loop_len + int(6 * SR))
    tonic = hz_to_midi(261.63)
    progression = [(0, 'maj'), (7, 'maj'), (9, 'min'), (5, 'maj')]
    melody_scale = [0, 2, 4, 7, 9, 12]  # ngũ cung trưởng

    for b in range(bars):
        off, quality = progression[b % 4]
        t0 = b * bar
        notes = chord_notes(tonic - 12 + off, quality)
        place(buf, piano_note(midi_to_hz(notes[0] - 12), bar * 1.2, 0.42, 0.7), t0)
        place(buf, piano_note(midi_to_hz(notes[2] - 12), bar * 1.1, 0.26, 0.7), t0 + beat * 0.02)
        arp = [notes[0] + 12, notes[1] + 12, notes[2] + 12, notes[1] + 12]
        for i, m in enumerate(arp):
            if rng.random() < 0.85:
                place(buf, piano_note(midi_to_hz(m), beat * 2.2, 0.22 + 0.05 * rng.random()), t0 + i * beat)
        if b % 2 == 1 and rng.random() < 0.8:
            m = tonic + 12 + int(rng.choice(melody_scale))
            place(buf, piano_note(midi_to_hz(m), beat * 3, 0.2, 1.1), t0 + beat * (2 + 0.5 * rng.integers(0, 2)))

    music = fold_loop(reverb(buf, seconds=3.2, wet=0.38), loop_len)
    room = loop_noise(rng, loop_len, high=900, tilt=-0.5)
    return master(music + 0.03 * np.max(np.abs(music)) * room)


def lofi_mom_care():
    """Lofi beat: A3, 64 bpm, Am7-Cmaj7-Em7-Dm7, trống nhẹ có swing, tiếng đĩa than."""
    rng = np.random.default_rng(64)
    bpm, bars = 64, 48
    beat = 60 / bpm
    bar = 4 * beat
    loop_len = int(bars * bar * SR)
    buf = np.zeros(loop_len + int(5 * SR))
    drums = np.zeros_like(buf)
    tonic = hz_to_midi(220.0)
    progression = [(0, 'min7'), (3, 'maj7'), (7, 'min7'), (5, 'min7')]
    swing = 0.06 * beat

    for b in range(bars):
        off, quality = progression[b % 4]
        t0 = b * bar
        notes = chord_notes(tonic + off, quality)
        for j, m in enumerate(notes):
            place(buf, keys_note(midi_to_hz(m), bar * 0.95, 0.2), t0 + j * 0.018)
        if rng.random() < 0.5:
            place(buf, keys_note(midi_to_hz(notes[-1] + 12), beat * 1.5, 0.12), t0 + 2.5 * beat + swing)
        root = midi_to_hz(tonic - 12 + off)
        place(buf, bass_note(root, beat * 1.9, 0.32), t0)
        place(buf, bass_note(root, beat * 1.4, 0.24), t0 + 2.5 * beat + swing)
        for k in range(4):
            if k in (0, 2):
                place(drums, kick(0.42 if k == 0 else 0.3), t0 + k * beat)
            if k in (1, 3):
                snare = spectral_filter(rng.standard_normal(int(0.18 * SR)), low=900, high=5000)
                ts = np.arange(len(snare)) / SR
                place(drums, 0.06 * snare * np.exp(-ts * 22), t0 + k * beat)
        for e in range(8):
            if rng.random() < 0.8:
                place(drums, hat(rng, 0.035 + 0.015 * rng.random()), t0 + e * beat / 2 + (swing if e % 2 else 0))

    music = reverb(spectral_filter(buf, high=4500), seconds=2.2, wet=0.3)
    drums = np.concatenate([drums, np.zeros(len(music) - len(drums))])
    mix = fold_loop(music + spectral_filter(drums, high=6000), loop_len)
    crackle = loop_impulses(rng, loop_len, 6, low=1500, high=7000)
    hiss = loop_noise(rng, loop_len, low=2000, high=8000)
    peak = np.max(np.abs(mix))
    return master(mix + peak * (0.05 * crackle + 0.015 * hiss))


def rain_piano():
    """Mưa nhỏ & piano xa: G3, 48 bpm, I-IV-vi-V, nền mưa rõ, piano thưa và xa."""
    rng = np.random.default_rng(48)
    bpm, bars = 48, 36
    beat = 60 / bpm
    bar = 4 * beat
    loop_len = int(bars * bar * SR)
    buf = np.zeros(loop_len + int(7 * SR))
    tonic = hz_to_midi(196.0)
    progression = [(0, 'maj'), (5, 'maj'), (9, 'min'), (7, 'maj')]

    for b in range(bars):
        off, quality = progression[b % 4]
        t0 = b * bar
        notes = chord_notes(tonic + off, quality)
        place(buf, piano_note(midi_to_hz(notes[0] - 12), bar * 1.3, 0.3, 0.6), t0)
        for i in range(3):
            if rng.random() < 0.55:
                m = notes[int(rng.integers(0, 3))] + 12
                place(buf, piano_note(midi_to_hz(m), beat * 3, 0.16, 0.8), t0 + (i * 1.3 + rng.random() * 0.4) * beat)

    piano = fold_loop(reverb(spectral_filter(buf, high=3200), seconds=4.0, wet=0.5), loop_len)
    rain = loop_noise(rng, loop_len, low=300, high=9000, tilt=-0.35)
    # Gió thổi lên xuống: số chu kỳ nguyên trong một vòng lặp để không lệch ở chỗ nối.
    cycles = max(1, round(loop_len / SR / 23.0))
    gust = 1 + 0.15 * np.sin(2 * np.pi * cycles * np.arange(loop_len) / loop_len)
    drops = loop_impulses(rng, loop_len, 40, low=1800, high=8000)
    peak = np.max(np.abs(piano))
    return master(piano + peak * (0.55 * rain * gust + 0.25 * drops))


def encode_mp3(samples, path):
    pcm = np.clip(samples * 32767, -32768, 32767).astype(np.int16).tobytes()
    enc = lameenc.Encoder()
    enc.set_bit_rate(128)
    enc.set_in_sample_rate(SR)
    enc.set_channels(1)
    enc.set_quality(2)
    path.write_bytes(enc.encode(pcm) + enc.flush())


def main():
    out_dir = Path(sys.argv[1])
    out_dir.mkdir(parents=True, exist_ok=True)
    for name, build in [
        ('piano-lullaby', piano_lullaby),
        ('lofi-mom-care', lofi_mom_care),
        ('rain-piano', rain_piano),
    ]:
        samples = build()
        path = out_dir / f'{name}.mp3'
        encode_mp3(samples, path)
        peak_db = 20 * np.log10(np.max(np.abs(samples)))
        rms_db = 20 * np.log10(np.sqrt(np.mean(samples ** 2)))
        print(f'{name}: {len(samples) / SR:.1f}s, peak {peak_db:.1f} dBFS, '
              f'rms {rms_db:.1f} dBFS, {path.stat().st_size / 1e6:.2f} MB')


if __name__ == '__main__':
    main()
