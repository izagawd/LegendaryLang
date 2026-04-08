struct Holder { val: i32 }
impl Copy for Holder {}
impl Holder {
    fn modify(self: &uniq Self) -> i32 { self.val }
}

struct Mid['a] {
    h: &'a uniq Holder
}

struct Top['a, 'b] {
    mid: &'a uniq Mid['b]
}

fn deep(t: &uniq Top) -> i32 {
    t.mid.h.modify()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let m = make Mid { h: &uniq h };
    let t = make Top { mid: &uniq m };
    deep(&uniq t)
}
