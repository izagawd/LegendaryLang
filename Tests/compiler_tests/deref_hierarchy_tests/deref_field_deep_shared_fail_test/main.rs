struct Holder { val: i32 }
impl Copy for Holder {}
impl Holder {
    fn modify(self: &uniq Self) -> i32 { self.val }
}

struct Inner['a] {
    h: &'a uniq Holder
}

struct Outer['a, 'b] {
    inner: &'a uniq Inner('b)
}

fn deep_modify(o: &Outer) -> i32 {
    o.inner.h.modify()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let i = make Inner { h: &uniq h };
    let o = make Outer { inner: &uniq i };
    deep_modify(&o)
}
