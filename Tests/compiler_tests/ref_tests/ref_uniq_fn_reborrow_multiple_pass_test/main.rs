fn add_five(r: &mut i32) {
    *r = *r + 5;
}

fn main() -> i32 {
    let x = 0;
    let r = &mut x;
    add_five(r);
    add_five(r);
    add_five(r);
    *r
}
