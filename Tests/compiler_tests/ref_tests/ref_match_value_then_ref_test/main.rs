enum Holder {
    Val(i32),
    Empty
}

fn read_ref(h: &Holder) -> i32 {
    match h {
        Holder.Val(x) => *x,
        Holder.Empty => 0
    }
}

fn main() -> i32 {
    let h = Holder.Val(50);
    let by_ref = read_ref(&h);
    let by_val = match h {
        Holder.Val(v) => v,
        Holder.Empty => 0
    };
    by_ref + by_val
}
