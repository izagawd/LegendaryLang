use Std.Ops.Drop;

struct Tracker { r: &mut i32 }

impl Drop for Tracker {
    fn Drop(self: &uniq Self) { *self.r = *self.r + 1; }
}

enum Maybe {
    Has(Tracker),
    Empty
}

fn main() -> i32 {
    let counter = 0;
    {
        let m: Maybe = Maybe.Empty;
    };
    counter
}
