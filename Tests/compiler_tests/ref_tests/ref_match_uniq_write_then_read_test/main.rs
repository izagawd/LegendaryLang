enum Holder {
    Val(i32),
    Empty
}

fn set(h: &uniq Holder, v: i32) {
    match h {
        Holder.Val(x) => *x = v,
        Holder.Empty => {}
    }
}

fn main() -> i32 {
    let h = Holder.Val(0);
    set(&uniq h, 42);
    set(&uniq h, 77);
    match h {
        Holder.Val(v) => v,
        Holder.Empty => 0
    }
}
