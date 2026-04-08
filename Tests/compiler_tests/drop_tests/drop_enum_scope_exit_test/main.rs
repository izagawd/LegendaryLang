use Std.Ops.Drop;

struct Tracker['a] { r: &'a mut i32 }

impl['a] Drop for Tracker['a] {
    fn Drop(self: &uniq Self) { *self.r = *self.r + 1; }
}

enum Holder['a] {
    Val(Tracker['a]),
    None
}

fn main() -> i32 {
    let counter = 0;
    {
        let h = Holder.Val(make Tracker { r: &mut counter });
    };
    counter
}
