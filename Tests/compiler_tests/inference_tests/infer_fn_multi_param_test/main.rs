struct Pair(A:! Sized, B:! Sized) {
    first: A,
    second: B
}

fn make_pair[A:! Sized +Copy, B:! Sized +Copy](a: A, b: B) -> Pair(A, B) {
    make Pair(A, B) { first : a, second : b }
}

fn main() -> i32 {
    let p = make_pair(10, 20);
    p.first + p.second
}
