use Std.Ops.Drop;

struct Tracker { r: &mut i32 }

impl Drop for Tracker {
    fn Drop(self: &uniq Self) { *self.r = *self.r + 1; }
}

enum Holder {
    Val(Tracker),
    Empty
}

fn main() -> i32 {
    let counter = 0;
    let before = counter;
    {
        let h = Holder.Val(make Tracker { r: &mut counter });
    };
    let after = counter;
    after
}
