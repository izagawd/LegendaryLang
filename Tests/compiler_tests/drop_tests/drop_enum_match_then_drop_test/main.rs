use Std.Ops.Drop;

struct Tracker['a] { r: &'a mut i32 }

impl['a] Drop for Tracker['a] {
    fn Drop(self: &mut Self) { *self.r = *self.r + 1; }
}

enum Wrapper['a] {
    Val(Tracker['a]),
    None
}

fn main() -> i32 {
    let counter = 0;
    {
        let w = Wrapper.Val(make Tracker { r: &mut counter });
        let tag = match w {
            Wrapper.Val(_) => 1,
            Wrapper.None => 0
        };
    };
    counter
}
