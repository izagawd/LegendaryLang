use Std.Core.Marker.Drop;
struct Guard['a] {
    r: &'a uniq i32
}

impl['a] Guard['a] {
    fn value(self: &Self) -> i32 {
        *self.r
    }
}

impl['a] Drop for Guard['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 100;
    }
}

fn main() -> i32 {
    let counter = 0;
    {
        let g = make Guard { r : &uniq counter };
        let v = g.value();
    }
    counter
}
