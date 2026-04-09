use Std.Ops.Drop;

struct Tracker['a] { r: &'a mut i32 }

impl['a] Drop for Tracker['a] {
    fn Drop(self: &uniq Self) { *self.r = *self.r + 1; }
}

enum Maybe['a] {
    Has(Tracker['a]),
    Empty
}

fn main() -> i32 {
    let counter = 0;
    {
        let m: Maybe = Maybe.Empty;
    };
    counter
}
