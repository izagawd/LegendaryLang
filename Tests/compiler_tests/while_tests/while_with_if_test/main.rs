fn main() -> i32 {
    let i = 0;
    let evens = 0;
    let odds = 0;
    while i < 10 {
        let rem = i - (i / 2) * 2;
        if rem == 0 {
            evens = evens + 1;
        } else {
            odds = odds + 1;
        };
        i = i + 1;
    };
    evens * 100 + odds
}
