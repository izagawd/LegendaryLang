fn set_val(r: &mut i32, v: i32) {
    *r = v;
}

fn main() -> i32 {
    let x = 0;
    set_val(&mut x, 55);
    x
}
