struct Inner['a] {
    r: &'a uniq i32
}

struct Outer['a] {
    inner: Inner['a]
}

impl['a] Outer['a] {
    fn set_val(self: &uniq Self, val: i32) {
        *self.inner.r = val;
    }
}

fn main() -> i32 {
    let x = 0;
    let o = make Outer { inner : make Inner { r : &uniq x } };
    o.set_val(55);
    x
}
