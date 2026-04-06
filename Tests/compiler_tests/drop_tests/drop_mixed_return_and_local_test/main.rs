use Std.Core.Marker.Drop;
struct Dropper {
    r: &mut i32
}

impl Drop for Dropper {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn keep_first(a: Dropper, b: Dropper) -> Dropper {
    a
}

fn consume(d: Dropper) {}

fn main() -> i32 {
    let counter_a = 0;
    let counter_b = 0;
    let kept = keep_first(
        make Dropper { r: &mut counter_a },
        make Dropper { r: &mut counter_b }
    );
    consume(kept);
    counter_a + counter_b
}
