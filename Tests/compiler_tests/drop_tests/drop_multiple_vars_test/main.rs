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
    let c3 = 0;
    {
        let d1 = make Dropper { r : &mut c1 };
        let d2 = make Dropper { r : &mut c2 };
        let d3 = make Dropper { r : &mut c3 };
    }
    c1 + c2 + c3
}
