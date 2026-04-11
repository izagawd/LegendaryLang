use Std.Ops.Drop;
struct Guard['a] {
    r: &'a mut i32
}

impl['a] Guard['a] {
    fn value(self: &Self) -> i32 {
        *self.r
    }
}

impl['a] Drop for Guard['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + 100;
    }
}

fn main() -> i32 {
    let counter = 0;
    {
        let g = make Guard { r : &mut counter };
        let v = g.value();
    }
    counter
}
