struct Holder {
    val: i32
}
impl Copy for Holder {}
impl Holder {
    fn modify(self: &uniq Self) -> i32 { self.val }
}

struct Wrapper['a] {
    inner: &'a uniq Holder
}

fn modify_through(w: &Wrapper) -> i32 {
    w.inner.modify()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let w = make Wrapper { inner: &uniq h };
    modify_through(&w)
}
