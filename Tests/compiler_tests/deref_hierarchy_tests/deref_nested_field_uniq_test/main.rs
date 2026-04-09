struct Holder {
    val: i32
}
impl Copy for Holder {}
impl Holder {
    fn modify(self: &uniq Self) -> i32 { self.val }
}

struct Middle['a] {
    target: &'a uniq Holder
}

struct Outer['a, 'b] {
    mid: &'a uniq Middle['b]
}

fn deep_modify(o: &uniq Outer) -> i32 {
    o.mid.target.modify()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let m = make Middle { target: &uniq h };
    let o = make Outer { mid: &uniq m };
    deep_modify(&uniq o)
}
