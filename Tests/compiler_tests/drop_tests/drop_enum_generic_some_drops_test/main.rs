use Std.Ops.Drop;

struct Tracker['a] { r: &'a mut i32 }

impl['a] Drop for Tracker['a] {
    fn Drop(self: &mut Self) { *self.r = *self.r + 1; }
}

fn main() -> i32 {
    let counter = 0;
    {
        let s = Option.Some(make Tracker { r: &mut counter });
    };
    counter
}
