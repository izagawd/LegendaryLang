struct Holder {
    val: i32
}
impl Copy for Holder {}
impl Holder {
    fn get(self: &Self) -> i32 { self.val }
}

struct Middle['a] {
    target: &'a uniq Holder
}

struct Outer['a, 'b] {
    mid: &'a uniq Middle('b)
}

fn deep_read(o: &Outer) -> i32 {
    o.mid.target.get()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let m = make Middle { target: &uniq h };
    let o = make Outer { mid: &uniq m };
    deep_read(&o)
}
