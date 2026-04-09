fn yo(dd: &mut i32) {
    *dd = 10 + *dd;
}

fn main() -> i32 {
    let val = 5;
    yo(&mut val);
    val
}
