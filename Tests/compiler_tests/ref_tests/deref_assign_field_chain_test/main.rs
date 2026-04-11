struct Inner['a] {
    r: &'a mut i32
}

struct Outer['a] {
    inner: Inner['a]
}

impl['a] Outer['a] {
    fn set_val(self: &mut Self, val: i32) {
        *self.inner.r = val;
    }
}

fn main() -> i32 {
    let x = 0;
    {
        let o = make Outer { inner : make Inner { r : &mut x } };
        o.set_val(55);
    };
    x
}
