use Std.Ops.Drop;

struct Tracker { r: &mut i32 }

impl Drop for Tracker {
    fn Drop(self: &uniq Self) { *self.r = *self.r + 1; }
}

enum Wrapper {
    Val(Tracker),
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
