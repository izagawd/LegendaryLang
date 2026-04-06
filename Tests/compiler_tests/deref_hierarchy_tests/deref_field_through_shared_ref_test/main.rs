struct Holder {
    val: i32
}
impl Copy for Holder {}
impl Holder {
    fn get(self: &Self) -> i32 { self.val }
}

struct Wrapper['a] {
    inner: &'a uniq Holder
}

fn read_through(w: &Wrapper) -> i32 {
    w.inner.get()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let w = make Wrapper { inner: &uniq h };
    read_through(&w)
}
