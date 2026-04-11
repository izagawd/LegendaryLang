use Std.Ops.Drop;
struct Inner {
    x: i32
}

struct Outer['a] {
    inner: Inner,
    r: &'a mut i32
}

impl['a] Drop for Outer['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + self.inner.x;
    }
}

fn main() -> i32 {
    let counter = 0;
    {
        let o = make Outer { inner : make Inner { x : 15 }, r : &mut counter };
    }
    counter
}
