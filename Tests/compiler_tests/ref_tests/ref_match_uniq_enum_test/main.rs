enum Holder {
    Val(i32)
}

fn set_inner(h: &uniq Holder, v: i32) {
    match h {
        Holder.Val(x) => *x = v,
        _ => {}
    }
}

fn main() -> i32 {
    let h = Holder.Val(0);
    set_inner(&uniq h, 99);
    match h {
        Holder.Val(v) => v,
        _ => 0
    }
}
