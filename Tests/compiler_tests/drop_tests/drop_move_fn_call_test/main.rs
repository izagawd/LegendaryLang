use Std.Core.Marker.Drop;
struct Dropper['a] {
    r: &'a uniq i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn take_dropper['a](d: Dropper['a]) -> i32 {
    0
}

fn main() -> i32 {
    let counter = 0;
    {
        let d = make Dropper { r : &uniq counter };
        take_dropper(d);
    }
    counter
}
