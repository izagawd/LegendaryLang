use Std.Core.Marker.Drop;
struct Guard['a] {
    target: &'a uniq i32
}

impl['a] Drop for Guard['a] {
    fn Drop(self: &uniq Self) {
        *self.target = *self.target + 100;
    }
}

fn main() -> i32 {
    let val = 0;
    {
        let g = make Guard { target : &uniq val };
    }
    val
}
