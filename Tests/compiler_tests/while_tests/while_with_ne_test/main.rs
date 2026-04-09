fn main() -> i32 {
    let i = 0;
    let count = 0;
    while i < 10 {
        if i != 5 {
            count = count + 1;
        };
        i = i + 1;
    };
    count
}
