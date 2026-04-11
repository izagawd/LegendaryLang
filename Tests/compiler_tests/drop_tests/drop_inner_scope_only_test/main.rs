use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + 1;
    }
}

fn main() -> i32 {
    let c1 = 0;
    let c2 = 0;
    {
        let inner = make Dropper { r : &mut c1 };
    }
    {
        let inner2 = make Dropper { r : &mut c2 };
    }
    c1 + c2
}
