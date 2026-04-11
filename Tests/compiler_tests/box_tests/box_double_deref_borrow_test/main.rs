fn borrower(dd: &mut Box(i32)) -> &mut i32 {
    &mut **dd
}

fn main() -> i32 {
    let dd: Box(i32) = Box.New(4);
    *borrower(&mut dd)
}
