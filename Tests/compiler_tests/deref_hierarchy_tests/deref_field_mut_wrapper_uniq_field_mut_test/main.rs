struct Holder { val: i32 }
impl Copy for Holder {}
impl Holder {
    fn get_mut(self: &mut Self) -> i32 { self.val }
}

struct Wrapper['a] {
    inner: &'a uniq Holder
}

fn read_mut(w: &mut Wrapper) -> i32 {
    w.inner.get_mut()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let w = make Wrapper { inner: &uniq h };
    read_mut(&mut w)
}
