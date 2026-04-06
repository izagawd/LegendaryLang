use Std.Ops.Drop;

struct Tracker { r: &mut i32 }

impl Drop for Tracker {
    fn Drop(self: &uniq Self) { *self.r = *self.r + 1; }
}

fn main() -> i32 {
    let counter = 0;
    {
        let n: Option(Tracker) = Option(Tracker).None;
    };
    counter
}
