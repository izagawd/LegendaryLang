struct Holder['a] {
    val: &'a mut i32
}

fn wrap(r: &mut i32) -> Holder {
    make Holder { val: r }
}

fn main() -> i32 {
    let x = 10;
    let h = wrap(&mut x);
    let y = x;
    *h.val + y
}
