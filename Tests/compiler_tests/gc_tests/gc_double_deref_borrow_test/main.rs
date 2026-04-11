fn borrower(dd: &mut Gc(i32)) -> &mut i32 {
    &mut **dd
}

fn main() -> i32 {
    let dd: Gc(i32) = Gc.New(4);
    *borrower(&mut dd)
}
